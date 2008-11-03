require 'gtk2'

module Cpg
	class ObjectList < Gtk::TreeView
		type_register
		
		signal_new('toggled',
				   GLib::Signal::RUN_LAST,
				   nil,
				   nil,
				   Object,
				   String)
	
		def initialize
			super
			
			@signals = {}

			build
		end
		
		def init_store
			child = @store.iter_first
			
			if child
				begin
					signals_unregister(child[0])
				end while child.next!
			end
			
			@store.clear
			
			Application.instance.grid.current.each do |obj|
				add_object(obj)
			end
		end
		
		def build
			@store = Gtk::TreeStore.new(Object, String, Integer)
			@store.set_sort_func(1) do |a, b|
				if !a[1]
					# objects before links
					aat = a[0].is_a?(Components::Attachment)
					bat = b[0].is_a?(Components::Attachment)
					
					if aat && !bat
						1
					elsif bat && !aat
						-1
					else
						a[0].to_s <=> b[0].to_s
					end
				else
					a[1].to_s <=> b[1].to_s
				end
			end
			
			@store.set_sort_column_id(1, Gtk::SORT_ASCENDING)
		
			self.model = @store
			self.headers_visible = false
			
			init_store
			self.expand_all
			
			# add two renderers, check, name
			renderer = Gtk::CellRendererToggle.new
			column = Gtk::TreeViewColumn.new('')

			column.pack_start(renderer, false)
			column.add_attribute(renderer, 'active', 2)

			self.append_column(column)
			
			renderer.signal_connect('toggled') do |renderer,path|
				piter = @store.get_iter(path)
				piter[2] = piter[2] == 1 ? 0 : 1
				
				if !piter[1]
					toggle_children(piter)
				else
					check_consistency(piter.parent)
					toggle_property(piter, piter[2] == 1)
				end
			end		
			
			column.set_cell_data_func(renderer) do |column, cell, model, piter|
				if !piter[1]
					cell.cell_background_gdk = self.style.base(Gtk::STATE_ACTIVE)
				else
					cell.cell_background_set = false
				end
				
				cell.inconsistent = piter[2] == 2
			end	
		
			renderer = Gtk::CellRendererText.new
			column.pack_start(renderer, true)

			column.set_cell_data_func(renderer) do |column, cell, model, piter|
				if !piter[1]
					s = "#{piter[0]} (#{piter[0].class.to_s.gsub(/.*::/, '')})"
					
					if piter[0].is_a?(Components::Link)
						s << " #{piter[0].from} » #{piter[0].to}"
					end
					
					cell.markup = "<b>#{s}</b>"
					cell.cell_background_gdk = self.style.base(Gtk::STATE_ACTIVE)
				else
					cell.text = piter[1].to_s
					cell.cell_background_set = false
				end
			end
			
			signal_register(Application.instance.grid, 'object_added') do |g, obj|
				# check needed because signal is also emitted from objects within
				# a group, but we don't want to show those
				add_object(obj) if Application.instance.grid.current.include?(obj)
			end
			
			signal_register(Application.instance.grid, 'object_removed') do |g, obj|
				remove_object(obj)
			end
		end
		
		def signal_do_destroy
			super
			
			# unregister signals
			@signals.keys.each { |obj| signals_unregister(obj) }
		end
		
		def signals_unregister(obj)
			@signals[obj].each { |x| obj.signal_handler_disconnect(x) }
			@signals.delete(obj)
		end
		
		def signal_register(obj, signal)
			@signals[obj] ||= []
			@signals[obj] << obj.signal_connect(signal) { |*args| yield *args }
		end
	
		def find(obj, prop = nil)
			parent = @store.iter_first
			return nil unless parent
			
			prop = prop.to_sym if prop
			
			begin
				if obj == parent[0]
					child = parent.first_child
					yielders = []
					
					begin
						yielders << Gtk::TreeRowReference.new(@store, child.path) if (prop == nil || child[1] == prop.to_s)
					end while child.next!
					
					if !yielders.empty?
						yielders.each { |x| yield @store.get_iter(x.path) } if block_given?
						return parent
					else
						return nil
					end
				end
			end while parent.next!
			
			nil
		end
		
		def add_property(parent, obj, prop)
			return if (obj.invisible?(prop) || prop == :id)
			
			piter = @store.append(parent)

			piter[0] = obj
			piter[1] = prop.to_s
			piter[2] = 0
		end
		
		def remove_object(obj)
			parent = find(obj)
			
			if parent
				@store.remove(parent)
			
				signals_unregister(obj)
			end
		end
		
		def add_object(obj)
			parent = @store.append(nil)
			parent[0] = obj
			parent[1] = nil
			parent[2] = 0
			
			obj.properties.each do |p|
				add_property(parent, obj, p)
			end
			
			check_consistency(parent)
			
			signal_register(obj, 'property_added') { |o, p| property_added(o, p) }
			signal_register(obj, 'property_removed') { |o, p| property_removed(o, p) }

			self.expand_row(parent.path, false)
		end
		
		def property_added(obj, prop)
			parent = find(obj)
			return unless parent
			
			add_property(parent, obj, prop.to_sym)
			check_consistency(parent)
		end
		
		def property_removed(obj, prop)
			parent = find(obj, prop) do |piter|
				@store.remove(piter)
			end

			check_consistency(parent) if parent
		end
		
		def toggle_property(child, active)
			child[2] = active ? 1 : 0
			signal_emit('toggled', child[0], child[1])
		end
		
		def active?(obj, prop)
			ret = true
			parent = find(obj, prop) { |x| ret = (ret && x[2] == 1) }
			
			return parent && ret
		end
		
		def set_active(obj, prop, active)
			find(obj, prop) { |x| x[2] = active ? 1 : 0}
		end
		
		def toggle_children(piter)
			find(piter[0]) { |x| toggle_property(x, piter[2] == 1) if x[2] != piter[2] }
		end
		
		def check_consistency(piter)
			child = piter.first_child
			return unless child

			numcheck = 0
			
			begin
				numcheck += child[2]
			end while child.next!
			
			if numcheck == 0
				piter[2] = 0
			elsif numcheck == piter.n_children
				piter[2] = 1
			else
				piter[2] = 2
			end
		end
		
		def signal_do_toggled(obj, prop)
		end
	end
end
