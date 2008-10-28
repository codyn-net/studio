require 'gtk2'
require 'propertyview'

module Cpg
	class PropertyEditor < Gtk::Dialog
		type_register
		
		attr_reader :object
		
		def initialize(parent, obj)
			super({'title' => "#{obj} - Property Editor"})
		
			destroy_with_parent = true
			set_transient_for(parent) if parent
			add_buttons([Gtk::Stock::CLOSE, Gtk::Dialog::RESPONSE_CLOSE])

			has_separator = false
			set_default_size(400, 300)

			if obj.is_a?(Components::Group)
				props = GroupProperties.new(obj.children, obj.main, obj.klass)
				
				vbox.pack_start(props, false, false, 0)
				vbox.pack_start(Gtk::HSeparator.new, false, false, 6)
				
				props.combo_main.signal_connect('changed') { |cmb| do_main_changed(cmb) }
				props.combo_class.signal_connect('changed') { |cmb| do_klass_changed(cmb) }
			end
			
			vbox.pack_start(PropertyView.new(obj), true, true, 0)
			@object = obj
			
			sid = @object.signal_connect('property_changed') do |obj, prop|
				s = "#{obj} - Property Editor"
				self.title = s if self.title != s
			end
			
			signal_connect('destroy') { |x| @object.signal_handler_disconnect(sid) }

			vbox.show_all
		end
		
		def do_main_changed(cmb)
			obj = cmb.active_iter ? cmb.active_iter[0] : nil
			@object.main = obj
		end
		
		def do_klass_changed(cmb)
			@object.klass = cmb.active_text
		end
	end
end
