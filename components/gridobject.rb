require 'allocation'
require 'glib2'
require 'serialize'
require 'range'

module Cpg::Components
	class Integrate < Array
		include Cpg::Serialize::Dynamic
	
		def properties
			self
		end
	
		def ensure_property(name)
			self << name.to_sym unless self.include?(name.to_sym)
		end
	
		def set_property(name, val)
			ensure_property(name)
		end
	
		def get_property(name)
			ensure_property(name)
			''
		end
	end

	class GridObject < GLib::Object
		include Cpg::Serialize::Dynamic
	
		type_register
	
		signal_new('request_redraw',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
	
		signal_new('property_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('property_removed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('property_added',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)

		attr_accessor :allocation, :selected, :links, :mousein, :focus
	
		def initialize
			super

			@allocation = Cpg::Allocation.new
			@selected = false
			@mousein = false
			@focus = false
			@links = []
		end
	
		def to_s
			@id.to_s
		end
		
		def mouse_enter
			@mousein = true
		end
		
		def mouse_exit
			@mousein = false
		end
	
		def state
			mystate = {}
			properties.each { |p| mystate[p] = get_property(p) }
		
			mystate
		end
		
		def draw_selection(ct)
			alpha = 0.2
	
			ct.rectangle(0, 0, allocation.width, allocation.height)
			ct.set_source_rgba(0, 0, 1, alpha)
	
			ct.fill_preserve
	
			ct.set_source_rgba(0, 0, 0, alpha)
			ct.stroke
		end
		
		def draw_focus(ct)
			uw = ct.line_width
			ct.line_width *= 0.5
			
			fw = 8
			dw = 2
			
			w, h = [allocation.width, allocation.height]
			
			ct.set_source_rgb(0, 0, 0)
			ct.move_to(-uw * dw, uw * fw)
			ct.line_to(-uw * dw, -uw * dw)
			ct.line_to(uw * fw, -uw * dw)
			ct.stroke
			
			ct.move_to(w - uw * fw, -uw * dw)
			ct.line_to(w + uw * dw, -uw * dw)
			ct.line_to(w + uw * dw, uw * fw)
			ct.stroke
			
			ct.move_to(w + uw * dw, h - uw * fw)
			ct.line_to(w + uw * dw, h + uw * dw)
			ct.line_to(w - uw * fw, h + uw * dw)
			ct.stroke
			
			ct.move_to(uw * fw, h + uw * dw)
			ct.line_to(-uw * dw, h + uw * dw)
			ct.line_to(-uw * dw, h - uw * fw)
			ct.stroke
		end
	
		def draw(ct)
			s = to_s
		
			if s && !s.empty?
				e = ct.text_extents(s)
			
				ct.set_source_rgb(0, 0, 0)
				ct.move_to((allocation.width - e.width) / 2.0, allocation.height + e.height * 2)
				ct.show_text(s)
			end

			draw_selection(ct) if @selected
			draw_focus(ct) if @focus
		end
	
		def animate(ts)
			false
		end
	
		def hittest(rect)
			true
		end
	
		def link(l)
			@links << l
		end
	
		def unlink(l)
			@links.delete(l)
		end
	
		def simulation_reset
		end
	
		def simulation_update(s)
		end
	
		def simulation_step(dt)
			{}
		end
		
		def add_property(prop)
			super
			signal_emit('property_added', prop.to_s)
		end
	
		def set_property(prop, val)
			if super and properties.include?(prop.to_sym)
				signal_emit('property_changed', prop.to_s)
			end
		end
	
		def remove_property(prop)
			if super
				signal_emit('property_removed', prop.to_s)
				return true
			end
		
			false
		end
	
		def request_redraw
			signal_emit('request_redraw')
		end
	
		def signal_do_request_redraw
		end
	
		def signal_do_property_changed(prop)
			if prop.to_sym == :id
				request_redraw
			end
		end
	
		def signal_do_property_removed(prop)
		end
		
		def signal_do_property_added(prop)
		end
	end

	class SimulatedObject < GridObject
		type_register

		signal_new('range_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('initial_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
			
		property :id, :integrate, :initial, :range, :allocation
		read_only :integrate, :initial, :range
		invisible :integrate, :initial, :range, :allocation
	
		def initialize
			super
		
			@integrate = Integrate.new
			@initial = Cpg::Hash.new
			@range = Cpg::Hash.new
		end
	
		def set_property(prop, val)
			super
		
			prop = prop.to_sym
			return if prop == :id

			if @initial[prop] == nil and !read_only?(prop) and !invisible?(prop)
				@initial[prop] = val
				signal_emit('property_changed', 'initial')
			end
		end
	
		def remove_property(prop)
			@initial.delete(prop) if super
		end
	
		def simulation_reset
			@initial.each do |k, v|
				set_property(k, v)
			end
		end
	
		def simulation_update(s)
			s.each do |prop, val|
				v = (integrated?(prop) ? get_property(prop) : 0) + val
				set_property(prop, v)
			end
		end
	
		def simulation_step(dt)
			mystate = {}
		
			@links.each do |l| 
				s = l.simulation_evaluate
			
				s.each do |k,v|
					mystate[k] = (mystate[k] || 0) + v * (integrated?(k) ? dt : 1)
				end
			end
		
			mystate
		end
	
		def set_integrated(name, value)
			if value
				@integrate << name.to_sym unless @integrate.include?(name.to_sym)
			else
				@integrate.delete(name.to_sym) if @integrate.include?(name.to_sym)
			end
		end
	
		def integrated?(name)
			@integrate.include?(name.to_sym)
		end
	
		def initial_value(prop)
			@initial.get_property(prop)
		end
	
		def set_initial_value(prop, val)
			@initial.set_property(prop, val)
			
			signal_emit('initial_changed', prop.to_s)
		end
	
		def get_range(prop)
			v = @range.get_property(prop)
			v && !v.empty? ? Cpg::Range.new(v) : nil
		end

		def set_range(prop, val)
			if val && !val.empty?
				val = Cpg::Range.normalize(val)
			else
				val = ''
			end

			@range.set_property(prop, val)
			signal_emit('range_changed', prop)
		end
		
		def signal_do_range_changed(prop)
		end
		
		def signal_do_initial_changed(prop)
		end
	end
end
