require 'components/simulatedobject'
require 'utils'

module Cpg::Components
	module Groups
		class Renderer
			module ClassMethods
				def inherited(base)
					base.extend(ClassMethods)
					
					cl = Renderer.instance_variable_get('@classes') || []
					cl << base
					
					Renderer.instance_variable_set('@classes', cl)
				end
			end
			
			extend(ClassMethods)
			
			def self.classes
				@classes || []
			end
			
			def animate(obj, ts)
				false
			end
			
			def draw(obj, ct)
				true
			end
		end
		
		class Default < Renderer
			def self.darken(r, g, b, a = 1.0)
				return [r * 0.6, g * 0.6, b * 0.6, a * 0.6]
			end

			def self.draw_rect(ct, x, y, width, height, c)
				ct.rectangle(x, y, width, height)
		
				if c.length == 3
					ct.set_source_rgb(*c)
				else
					ct.set_source_rgba(*c)
				end
		
				ct.fill_preserve
		
				ct.set_source_rgba(*darken(*c))
				ct.stroke
			end
			
			def self.draw(obj, ct)
				allocation = obj.allocation
	
				ct.save
				uw = ct.line_width
				ct.line_width = uw * 2
		
				off = uw * 2 + allocation.width * 0.1
				w = (allocation.width - 2 * off) * 0.4
				h = (allocation.height - 2 * off) * 0.4
		
				draw_rect(ct, off, off, w, h, [26 / 125.0, 80 / 125.0, 130 / 125.0])
				draw_rect(ct, off, allocation.height - h - off, w, h, [80 / 125.0, 26 / 125.0, 130 / 125.0])
				draw_rect(ct, allocation.width - w - off, allocation.height - h - off, w, h, [26 / 125.0, 130 / 125.0, 80 / 125.0])
				draw_rect(ct, allocation.width - w - off, off, w, h, [130 / 125.0, 80 / 125.0, 26 / 125.0])
		
				w = (allocation.width - 2 * off) * 0.5
				h = (allocation.height - 2 * off) * 0.5
		
				x = (allocation.width - w) / 2.0
				y = (allocation.height - h) / 2.0

				draw_rect(ct, x, y, w, h, [80.0 / 125.0, 130.0 / 125.0, 26.0 / 125.0])
		
				ct.restore
				true
			end
			
			def draw(obj, ct)
				self.class.draw(obj, ct)
			end
		end
	end

	class Group < SimulatedObject
		include Enumerable

		attr_accessor :children, :__x, :__y, :__main, :__klass

		property :__main, :__klass, :__x, :__y
		invisible :__klass, :__x, :__y, :__main
		read_only :__main, :__klass, :__x, :__y
		
		FADE_MIN = 0.1
	
		def initialize
			super
		
			@children = Cpg::Array.new
			@renderer = nil

			self.__main = nil
			self.__klass = nil
			self.__x = 0
			self.__y = 0
			
			@fadetrans = 1
			@signals = []
		end
		
		def __main
			@properties['__main']
		end
		
		def __klass
			@properties['__klass']
		end
		
		def __x
			@properties['__x']
		end
		
		def __y
			@properties['__y']
		end
		
		alias :main :__main
		alias :main= :__main=
		alias :x :__x
		alias :y :__y
		alias :klass :__klass
		alias :klass= :__klass=
		alias :x= :__x=
		alias :y= :__y=
	
		def mouse_enter
			super
			
			GLib::Source.remove(@waitsource) if @waitsource
			@waitsource = GLib::Timeout.add(500) do
				@fadetrans = 1 if @fadetrans > 1
				@waitsource = nil
				false
			end
			
			request_redraw
		end
		
		def mouse_exit
			super
			
			GLib::Source.remove(@waitsource) if @waitsource
			@waitsource = nil
			request_redraw
		end
	
		def dup
			o = super
			o.children = o.children.dup
		
			o
		end
		
		def delete(o)
			@children.delete(o)
		end
	
		def delete_if
			@children.delete_if { |x| yield x }
		end

		def reverse
			@children.reverse
		end
	
		def [](idx)
			@children[idx]
		end
	
		def <<(val)
			@children << val
			@children.sort!
		
			self
		end
	
		def []=(idx, val)
			@children[idx] = val
		end
	
		def sort
			@children.sort { |x| yield x }
		end
	
		def sort!
			@children.sort! { |x| yield x }
			self
		end
	
		def each
			@children.each { |child| yield child }
		end
		
		def length
			@children.length
		end
		
		def index(o)
			@children.index(o)
		end
		
		def draw_children(ct)
			# do some cool magic where we draw our children
			x = []
			y = []
			
			@children.each do |child|
				next if child.is_a?(Attachment)
				
				x << child.allocation.x << child.allocation.x + child.allocation.width
				y << child.allocation.y << child.allocation.y + child.allocation.height
			end
			
			# x and y now contain points for all children, lets scale and
			# transform xmin/xmax, ymin/ymax, to our own x/y coordinates
			scale = 0.8
			factor = [allocation.width / (x.max - x.min).to_f, allocation.height / (y.max - y.min).to_f].min * scale
			
			ct.scale(factor, factor)
			ct.translate(-x.min + (allocation.width / factor - (x.max - x.min)) * 0.5, -y.min + (allocation.height / factor - (y.max - y.min)) * 0.5)
			
			@children.sort.reverse.each do |child|
				ct.save
				ct.translate(child.allocation.x, child.allocation.y)

				child.draw(ct)
				ct.restore
			end
		end
		
		def do_own_draw(ct)
			proc = SimulatedObject.instance_method(:draw).bind(self)
			
			if @renderer and @renderer.respond_to?(:draw)
				proc.call(ct) if @renderer.draw(self, ct)
			else
				proc.call(ct) if Cpg::Components::Groups::Default.draw(self, ct)
			end
		end
	
		def draw(ct)
			if @fadetrans < 1
				ct.push_group
				draw_children(ct)
				
				ct.pop_group_to_source
				ct.mask(Cairo::SolidPattern.new(1, 1, 1, 1 - @fadetrans))
				
				ct.push_group
				do_own_draw(ct)
				
				ct.pop_group_to_source
				ct.mask(Cairo::SolidPattern.new(1, 1, 1, @fadetrans))
			else
				do_own_draw(ct)
			end
		end
		
		def animate(ts)
			ret = @renderer && @renderer.respond_to?(:animate) && @renderer.animate(self, ts)
			
			if (@mousein && @fadetrans > FADE_MIN && !@waitsource) || (!@mousein && @fadetrans < 1)
				@fadetrans *= (@mousein ? 1 / 1.2 : 1.2)
				return true
			end
			
			return ret
		end
		
		def set_klass_real(klass)
			if klass.is_a?(Class) && klass.ancestors.include?(Cpg::Components::Groups::Renderer)
				@renderer = klass.new
			elsif klass.is_a?(String)
				if Cpg::Components::Groups.const_defined?(klass)
					@renderer = Cpg::Components::Groups.const_get(klass).new
				else
					@renderer = nil
				end
			else
				@renderer = nil
			end
			
			request_redraw
		end
	
		def my_property(name)
			@properties.include?(name)
		end
		
		# proxy properties to main object
		def save_properties
			@properties.keys
		end
		
		def custom_properties
			a = @properties.keys.dup
			a.delete_if { |x| class_properties.include?(x) }
			
			a
		end
	
		def properties
			if self.__main
				ret = self.__main.properties.merge(super)
			else
				ret = super
			end
			
			ret
		end
	
		def simulation_reset
			# do our children
			@children.each { |child| child.simulation_reset }
		
			# and ourselves
			super
		end
	
		def simulation_update(s)
			# do our children, but not main because it will be actually updated using
			# our own state (since properties are relayed)
			@children.each { |child| child.simulation_update(@childstate[child]) unless child == self.__main }
		
			# and ourselves
			super
		end
	
		def simulation_step(dt)
			# process connections to this group node
			mystate = super
		
			# process all the children individually
			@childstate = {}
			@children.each { |child| @childstate[child] = child.simulation_step(dt) }
		
			# make sure to merge our state and the main one (it's like a special
			# link)
			return mystate unless @__main && @childstate.include?(self.__main)
			cs = @childstate[self.__main]
		
			cs.each do |k, v|
				mystate[k] = (mystate[k] || 0) + (v || 0)
			end
		
			mystate
		end
	
		def relay_signal(sname)
			self.__main.signal_connect(sname) { |o, *a| signal_emit(sname, *a) }
		end
		
		def set_property_main(val)
			return nil if val == self.__main

			# disconnect relay signals
			@signals.each { |s| self.__main.signal_handler_disconnect(s) } if self.__main
			@signals = []
			
			# we now 'virtually' remove all properties of the current main
			if self.__main
				self.__main.properties.each do |p|
					signal_emit('property_removed', p.to_s) unless my_property(p)
				end
			end

			v = SimulatedObject.instance_method(:set_property).bind(self).call('__main', val)
	
			if self.__main
				# setup relay signals
				@signals << relay_signal('property_changed')
				@signals << relay_signal('property_removed')
				
				@signals << relay_signal('range_changed')
				@signals << relay_signal('initial_changed')
				
				# and signal property changes for all properties in __main
				self.__main.properties.each do |p|
					signal_emit('property_changed', p.to_s) unless my_property(p)
				end
			end
		
			return v
		end
	
		def set_property(prop, val)
			if prop == '__main'
				return set_property_main(val)
			elsif prop == '__klass'
				set_klass_real(val)
				return super
			end
		
			return super if (my_property(prop) or !self.__main or !self.__main.properties.include?(prop))
			self.__main.set_property(prop, val) if self.__main
		end
		
		def self.propagate(func)
			send :define_method, func do |*a|
				return super if (my_property(a[0]) or (self.__main && !self.__main.properties.include?(a[0])))
				self.__main.send(func, *a) if self.__main
			end
		end	
	
		propagate :set_initial_value
		propagate :initial_value
		propagate :get_property
		propagate :read_only?
		propagate :invisible?
		propagate :property_set?
		propagate :set_integrated
		propagate :integrated?
		propagate :set_range
		propagate :get_range
		propagate :class_property?
	end
end
